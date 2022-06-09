/*
 * PackageCallback.cs --
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
    [ObjectId("6369a999-b50c-42c5-909d-caf222bd2148")]
    public interface IPackageCallback
    {
        ReturnCode PackageFallback(
            Interpreter interpreter, // TODO: Change to use the IInterpreter type.
            string name,
            Version version,
            string text,
            PackageFlags flags,
            bool exact,
            ref Result result
        );
    }
}
