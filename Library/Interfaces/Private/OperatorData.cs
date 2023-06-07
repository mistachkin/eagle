/*
 * OperatorData.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    [ObjectId("f9854ec8-39f3-489a-a32e-7da95a51e264")]
    internal interface IOperatorData : IIdentifier, IWrapperData, IHavePlugin, ITypeAndName
    {
        Lexeme Lexeme { get; set; }
        int Operands { get; set; }
        TypeList Types { get; set; }
        OperatorFlags Flags { get; set; }
        StringComparison ComparisonType { get; set; }
    }
}
