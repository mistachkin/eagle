/*
 * ProcessHost.cs --
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
    [ObjectId("64ebb0dc-03a6-4860-8b85-350af13bd36d")]
    public interface IProcessHost : IInteractiveHost
    {
        bool CanExit { get; set; }
        bool CanForceExit { get; set; }
        bool Exiting { get; set; }
    }
}
