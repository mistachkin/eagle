/*
 * ScriptMarshalByRefObject.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Security.Permissions;
using Eagle._Attributes;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class serves as the base class for objects that may be marshaled
    /// by reference across application domain boundaries.  It customizes the
    /// .NET Remoting lifetime service so that derived objects are never
    /// reclaimed due to lease expiration.
    /// </summary>
    [ObjectId("96fc62ff-d9d8-4c5a-81f6-5f59633b8e85")]
    public class ScriptMarshalByRefObject : MarshalByRefObject
    {
        /// <summary>
        /// This method obtains the lifetime service object used to control the
        /// remoting lease policy for this instance.
        /// </summary>
        /// <returns>
        /// A null reference, which causes this object to have an infinite
        /// lifetime (its remoting lease never expires).
        /// </returns>
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null; /* INFINITE */
        }
    }
}
