/*
 * PolicyEnsemble.cs --
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
    [ObjectId("4232d0b8-22b7-4738-9c69-4b751d44dec4")]
    public interface IPolicyEnsemble
    {
        EnsembleDictionary AllowedSubCommands { get; set; }
        EnsembleDictionary DisallowedSubCommands { get; set; }
    }
}
