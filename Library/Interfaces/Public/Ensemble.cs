/*
 * Ensemble.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("c2a807b0-f6e2-4c06-b856-dbb37944fabd")]
    public interface IEnsemble
    {
        EnsembleDictionary SubCommands { get; set; }
    }
}
