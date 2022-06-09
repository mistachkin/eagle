/*
 * Identifier.cs --
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
    [ObjectId("513d51f3-f12a-48d7-a26b-02e27734bd50")]
    public interface IIdentifier : IIdentifierBase, IHaveClientData
    {
        //
        // NOTE: The logical group this identifier belongs to.
        //
        string Group { get; set; }

        //
        // NOTE: The description of this identifier.
        //
        string Description { get; set; }
    }
}
