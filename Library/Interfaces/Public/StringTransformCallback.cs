/*
 * StringTransformCallback.cs --
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
    [ObjectId("2fde7f63-d35f-4592-8c43-dbee8bb3cb0f")]
    public interface IStringTransformCallback
    {
        string StringTransform(
            string value
        );
    }
}
