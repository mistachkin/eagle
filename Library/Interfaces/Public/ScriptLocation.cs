/*
 * ScriptLocation.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("2b42339e-bc7c-40bf-91bc-637e63006842")]
    public interface IScriptLocation
    {
        string FileName { get; set; }
        int StartLine { get; set; }
        int EndLine { get; set; }
        bool ViaSource { get; set; }

        StringPairList ToList();
        StringPairList ToList(bool scrub);
    }
}
