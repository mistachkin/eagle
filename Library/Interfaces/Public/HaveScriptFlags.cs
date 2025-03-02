/*
 * HaveScriptFlags.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("c753ddc8-fa43-4f3f-881d-1eba30210af6")]
    public interface IHaveScriptFlags
    {
        EngineMode EngineMode { get; set; }
        ScriptFlags ScriptFlags { get; set; }
        EngineFlags EngineFlags { get; set; }
        SubstitutionFlags SubstitutionFlags { get; set; }
        EventFlags EventFlags { get; set; }
        ExpressionFlags ExpressionFlags { get; set; }
    }
}
