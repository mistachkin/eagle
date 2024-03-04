/*
 * BaseClientData.cs --
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
    [ObjectId("30e24a85-0fcb-449a-811a-585ad8e0c03c")]
    public interface IBaseClientData
    {
        object DataNoThrow { get; set; }
        bool ReadOnly { get; }

        IClientData Log { get; set; }
    }
}
