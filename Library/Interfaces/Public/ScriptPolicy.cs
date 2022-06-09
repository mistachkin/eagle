/*
 * ScriptPolicy.cs --
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
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("d3146201-50a4-4671-b34b-f2cacb7a06ef")]
    public interface IScriptPolicy : IExecute
    {
        PolicyFlags Flags { get; }
        Type CommandType { get; }
        long CommandToken { get; }
        Interpreter PolicyInterpreter { get; }
        string Text { get; }
    }
}
