/*
 * IdentifierName.cs --
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
    [ObjectId("dfdc14a1-94be-45bc-bdab-f0df1623eeb9")]
    public interface IIdentifierName
    {
        //
        // NOTE: The name of this identifier.
        //
        string Name { get; set; }
    }
}
