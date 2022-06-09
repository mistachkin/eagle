/*
 * ProcedureData.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("16dc0c56-ed0a-4e41-9797-3a9ae2af7e13")]
    public interface IProcedureData : IIdentifier, IWrapperData
    {
        ProcedureFlags Flags { get; set; }
        ArgumentList Arguments { get; set; }
        ArgumentDictionary NamedArguments { get; set; }
        string Body { get; set; }
        IScriptLocation Location { get; set; }
    }
}
