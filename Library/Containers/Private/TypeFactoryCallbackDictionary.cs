/*
 * TypeFactoryCallbackDictionary.cs --
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
using Eagle._Components.Private.Delegates;

namespace Eagle._Containers.Private
{
    [ObjectId("ef362c7e-fe48-479c-85d6-913f828abd36")]
    internal sealed class TypeFactoryCallbackDictionary :
        Dictionary<Type, FactoryCallback>
    {
        public TypeFactoryCallbackDictionary()
            : base()
        {
            // do nothing.
        }
    }
}
