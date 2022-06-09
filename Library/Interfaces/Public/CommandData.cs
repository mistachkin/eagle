/*
 * CommandData.cs --
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
    [ObjectId("28b3f497-67e5-47bd-9883-b574cb0bc653")]
    public interface ICommandData : IIdentifier, ICommandBaseData, IHavePlugin, IWrapperData
    {
        //
        // NOTE: The flags for this command.
        //
        CommandFlags Flags { get; set; }
    }
}
