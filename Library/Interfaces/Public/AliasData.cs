/*
 * AliasData.cs --
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
    [ObjectId("6291616e-faba-4cf2-8f9d-6ce746adaf3b")]
    public interface IAliasData : IIdentifier, IWrapperData
    {
        AliasFlags AliasFlags { get; set; }
        string NameToken { get; set; }
        Interpreter SourceInterpreter { get; set; } // TODO: Change this to use the IInterpreter type.
        Interpreter TargetInterpreter { get; set; } // TODO: Change this to use the IInterpreter type.
        INamespace SourceNamespace { get; set; }
        INamespace TargetNamespace { get; set; }
        IExecute Target { get; set; }
        ArgumentList Arguments { get; set; }
        OptionDictionary Options { get; set; }
        int StartIndex { get; set; }
    }
}
