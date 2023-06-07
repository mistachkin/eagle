/*
 * HaveEncoding.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("01eb0013-a2ab-4839-9b57-6ecfc13782fb")]
    public interface IHaveEncoding
    {
        Encoding Encoding { get; set; }
    }
}
