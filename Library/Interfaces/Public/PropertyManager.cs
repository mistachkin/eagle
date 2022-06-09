/*
 * PropertyManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("fe6b5edb-dc7d-45dd-a4d8-421e713734af")]
    public interface IPropertyManager
    {
        ///////////////////////////////////////////////////////////////////////
        // MISCELLANEOUS DATA
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: RESERVED for use by the host application.
        //
        object ApplicationObject { get; set; }

        //
        // NOTE: RESERVED for use by the custom policies.
        //
        object PolicyObject { get; set; }

        //
        // NOTE: RESERVED for use by the custom resolvers.
        //
        object ResolverObject { get; set; }

        //
        // NOTE: RESERVED for use by the host application.
        //
        object UserObject { get; set; }
    }
}
