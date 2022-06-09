/*
 * IdentifierBase.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("cb8c4833-74ff-48b4-9991-689317a6c9da")]
    public interface IIdentifierBase : IIdentifierName
    {
        //
        // NOTE: The enumerated kind of this identifier
        //       (i.e. command, plugin, etc).
        //
        IdentifierKind Kind { get; set; }

        //
        // NOTE: The unique Id of this identifier.
        //
        Guid Id { get; set; }
    }
}
