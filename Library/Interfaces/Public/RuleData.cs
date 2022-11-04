/*
 * RuleData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("c274c1b6-e844-4577-adb9-e213a80d82f6")]
    public interface IRuleData
    {
        long? Id { get; }
        RuleType Type { get; }
        IdentifierKind Kind { get; }
        MatchMode Mode { get; }
        RegexOptions RegExOptions { get; }
        IEnumerable<string> Patterns { get; }
        IComparer<string> Comparer { get; }
    }
}
