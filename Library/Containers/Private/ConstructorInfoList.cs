/*
 * ConstructorInfoList.cs --
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
    [ObjectId("1ef27ad5-f39d-4146-b26c-3ac39caf01c1")]
    internal sealed class ConstructorInfoList : List<ConstructorInfo>
    {
        public ConstructorInfoList()
            : base()
        {
            // do nothing.
        }
    }
}
