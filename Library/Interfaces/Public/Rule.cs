/*
 * Rule.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("83ce9774-7280-40bc-8cf2-134b19b657f7")]
    public interface IRule : IRuleData, ICloneable
    {
        void SetId(long? id);
    }
}
