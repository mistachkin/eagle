/*
 * MethodBaseList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Reflection;
using Eagle._Attributes;

namespace Eagle._Containers.Private
{
    [ObjectId("3f0fe2eb-2331-4cd4-8d68-7fe89abaae22")]
    internal sealed class MethodBaseList : List<MethodBase>
    {
        public MethodBaseList()
            : base()
        {
            // do nothing.
        }
    }
}
