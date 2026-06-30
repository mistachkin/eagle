/*
 * AppDomainDictionary.cs --
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

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, System.AppDomain>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, System.AppDomain>;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string names to
    /// application domain instances.
    /// </summary>
    [ObjectId("e595f3de-cab4-4a0d-a4d5-b84d55087fd0")]
    internal sealed class AppDomainDictionary : SomeDictionary
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public AppDomainDictionary()
            : base()
        {
            // do nothing.
        }
    }
}
