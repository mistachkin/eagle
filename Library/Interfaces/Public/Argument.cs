/*
 * Argument.cs --
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
    [ObjectId("dda1842c-b846-431b-8692-e87cea73d555")]
    public interface IArgument : IGetValue, IValueData
    {
        string Name { get; set; }
        ArgumentFlags Flags { get; set; }
        object Default { get; set; }
        void Reset(ArgumentFlags flags);

        bool HasFlags(ArgumentFlags hasFlags, bool all);
    }
}
