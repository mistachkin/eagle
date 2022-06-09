/*
 * InteractiveManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("95afaaa3-ddc1-423d-9b4a-c6b6464fb689")]
    public interface IInteractiveManager
    {
        ///////////////////////////////////////////////////////////////////////
        // INTERACTIVE SUPPORT
        ///////////////////////////////////////////////////////////////////////

        IInteractiveHost InteractiveHost { get; set; }
        bool Interactive { get; set; }
        string InteractiveInput { get; set; }
        string PreviousInteractiveInput { get; set; }
        string InteractiveMode { get; set; }

        StringTransformCallback InteractiveCommandCallback { get; set; }
    }
}
