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
    [ObjectId("96fc62ff-d9d8-4c5a-81f6-5f59633b8e85")]
    public class ScriptMarshalByRefObject : MarshalByRefObject
    {
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null; /* INFINITE */
        }
    }
}
