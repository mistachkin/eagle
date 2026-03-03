/*
 * ViaScript.cs --
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

namespace Eagle._Interfaces.Private
{
    [ObjectId("8ec9cf8f-7237-4a5a-b73b-2c1c32cda6e2")]
    internal interface IViaScript
    {
        bool IsViaScript { get; }
    }
}
