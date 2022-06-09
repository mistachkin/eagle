/*
 * ScriptBuilder.cs --
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
    [ObjectId("14e75abb-1d52-4c3d-9d65-299f1665a475")]
    public interface IScriptBuilder : IIdentifier
    {
        int Count { get; }

        ReturnCode Clear(ref Result error);
        ReturnCode Add(string text, ref Result error);
        ReturnCode Add(IStringList arguments, ref Result error);
        ReturnCode Add(IScript script, ref Result error);
        ReturnCode Add(IScriptBuilder builder, ref Result error);

        string GetString(bool nested);
        IScript GetScript(bool nested);
    }
}
