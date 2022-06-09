/*
 * HaveCommand.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("bdd98630-36f9-4fc4-a9f0-e42c9d4f8bb8")]
    public interface IHaveCommand
    {
        ICommand Command { get; set; }
    }
}
