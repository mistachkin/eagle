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

namespace Eagle._Containers.Private
{
    [ObjectId("e595f3de-cab4-4a0d-a4d5-b84d55087fd0")]
    internal sealed class AppDomainDictionary : Dictionary<string, AppDomain>
    {
        public AppDomainDictionary()
            : base()
        {
            // do nothing.
        }
    }
}

