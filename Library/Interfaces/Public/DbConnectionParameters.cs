/*
 * DbConnectionParameters.cs --
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
    [ObjectId("7cc7b141-f01d-4acd-ba59-8a717680d913")]
    public interface IDbConnectionParameters
    {
        DbConnectionType DbConnectionType { get; set; }
        string ConnectionString { get; set; }
        string AssemblyFileName { get; set; }
        string TypeFullName { get; set; }
        string TypeName { get; set; }
        ValueFlags ValueFlags { get; set; }
    }
}
