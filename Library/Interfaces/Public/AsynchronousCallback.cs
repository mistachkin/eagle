/*
 * AsynchronousCallback.cs --
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
    [ObjectId("9e7e939e-766e-44d3-83af-2d00f361a3da")]
    public interface IAsynchronousCallback
    {
        void Invoke(IAsynchronousContext context);
    }
}
