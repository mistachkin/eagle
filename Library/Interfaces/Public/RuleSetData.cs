/*
 * RuleSetData.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("4a22a525-8403-48e1-b0b1-6ac3b022c8e4")]
    public interface IRuleSetData
    {
        IComparer<string> Comparer { get; set; }
    }
}
