/*
 * UpdateData.cs --
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
using Eagle._Components.Shared;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    //
    // NOTE: This interface is currently private; however, it may be "promoted"
    //       to public at some point.
    //
    [ObjectId("c3510a45-bb25-46b4-ae84-f7dc15ec20aa")]
    internal interface IUpdateData : IIdentifier
    {
        string TargetDirectory { get; set; }

        Uri Uri { get; set; }
        byte[] PublicKeyToken { get; set; }
        string Culture { get; set; }

        Version PatchLevel { get; set; }
        DateTime? TimeStamp { get; set; }

        ActionType ActionType { get; set; }
        ReleaseType ReleaseType { get; set; }
        UpdateType UpdateType { get; set; }

        bool WantScripts { get; set; }
        bool Quiet { get; set; }
        bool Prompt { get; set; }
        bool Automatic { get; set; }
    }
}
