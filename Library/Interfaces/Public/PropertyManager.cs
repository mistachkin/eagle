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
    /// <summary>
    /// This interface defines a set of general-purpose, reserved object
    /// properties that may be associated with an entity for use by the host
    /// application, custom policies, and custom resolvers.  The Eagle core does
    /// not interpret the values of these properties.
    /// </summary>
    [ObjectId("fe6b5edb-dc7d-45dd-a4d8-421e713734af")]
    public interface IPropertyManager
    {
        ///////////////////////////////////////////////////////////////////////
        // MISCELLANEOUS DATA
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: RESERVED for use by the host application.
        //
        /// <summary>
        /// Gets or sets an arbitrary object reserved for use by the host
        /// application.
        /// </summary>
        object ApplicationObject { get; set; }

        //
        // NOTE: RESERVED for use by the custom policies.
        //
        /// <summary>
        /// Gets or sets an arbitrary object reserved for use by custom
        /// policies.
        /// </summary>
        object PolicyObject { get; set; }

        //
        // NOTE: RESERVED for use by the custom resolvers.
        //
        /// <summary>
        /// Gets or sets an arbitrary object reserved for use by custom
        /// resolvers.
        /// </summary>
        object ResolverObject { get; set; }

        //
        // NOTE: RESERVED for use by the host application.
        //
        /// <summary>
        /// Gets or sets an arbitrary object reserved for use by the host
        /// application.
        /// </summary>
        object UserObject { get; set; }
    }
}
