/*
 * ShellManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("0b941a5b-cf8c-40ae-850a-59249899cd37")]
    public interface IShellManager
    {
        PreviewArgumentCallback PreviewArgumentCallback { get; set; }
        UnknownArgumentCallback UnknownArgumentCallback { get; set; }
        EvaluateScriptCallback EvaluateScriptCallback { get; set; }
        EvaluateFileCallback EvaluateFileCallback { get; set; }
        EvaluateEncodedFileCallback EvaluateEncodedFileCallback { get; set; }
    }
}
