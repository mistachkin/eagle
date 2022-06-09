/*
 * ChangeTypeData.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("0026a039-64df-417b-9d36-506ecf04d904")]
    public interface IChangeTypeData : IHaveCultureInfo
    {
        string Caller { get; }
        Type Type { get; }
        object OldValue { get; }
        OptionDictionary Options { get; }
        IClientData ClientData { get; }

        MarshalFlags MarshalFlags { get; set; }
        object NewValue { get; set; }

        bool NoHandle { get; set; }
        bool WasObject { get; set; }
        bool Attempted { get; set; }
        bool Converted { get; set; }
        bool DoesMatch { get; set; }
    }
}
