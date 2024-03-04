/*
 * HaveFileName.cs --
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
    [ObjectId("bef2dd92-e272-4bb1-be4c-9e628f2a1045")]
    public interface IHaveFileName
    {
        string FileName { get; set; }
    }
}
