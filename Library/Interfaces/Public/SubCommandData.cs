/*
 * SubCommandData.cs --
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
    [ObjectId("7f9e86b1-36c0-4699-97ee-5a86f3859a12")]
    public interface ISubCommandData : IIdentifier, ICommandBaseData, IHaveCommand, IWrapperData
    {
        int NameIndex { get; set; }
        SubCommandFlags Flags { get; set; }
    }
}
