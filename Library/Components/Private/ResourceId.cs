/*
 * ResourceId.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This enumeration identifies a named string resource that may be looked
    /// up within the resources associated with an interpreter.
    /// </summary>
    [ObjectId("2bcdce20-b952-464d-98b8-f600851a4633")]
    internal enum ResourceId
    {
        /// <summary>
        /// Represents an invalid resource identifier.
        /// </summary>
        Invalid = -1,

        /// <summary>
        /// Represents the absence of any resource identifier.
        /// </summary>
        None = 0,

        /// <summary>
        /// Represents a resource identifier used for testing purposes.
        /// </summary>
        Test = 1,

        // ** BEGIN GENERATED CODE **
        // ** END GENERATED CODE **
    }
}
