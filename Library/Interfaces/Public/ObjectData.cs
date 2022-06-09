/*
 * ObjectData.cs --
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

#if DEBUGGER && DEBUGGER_ARGUMENTS
using Eagle._Containers.Public;
#endif

namespace Eagle._Interfaces.Public
{
    [ObjectId("a2691e49-85f6-4df3-8725-3aded340e6eb")]
    public interface IObjectData : IIdentifier, IHaveObjectFlags, IWrapperData
    {
        Type Type { get; set; }

        IAlias Alias { get; set; }

        int ReferenceCount { get; set; }
        int TemporaryReferenceCount { get; set; }

#if NATIVE && TCL
        string InterpName { get; set; }
#endif

#if DEBUGGER && DEBUGGER_ARGUMENTS
        ArgumentList ExecuteArguments { get; set; }
#endif
    }
}
