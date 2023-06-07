/*
 * HostData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("35cfe935-a23e-48ce-a395-2dab9c268c2f")]
    public interface IHostData : IIdentifier, IHaveInterpreter, ITypeAndName
    {
        ResourceManager ResourceManager { get; set; }
        string Profile { get; set; }
        HostCreateFlags HostCreateFlags { get; set; }
    }
}
