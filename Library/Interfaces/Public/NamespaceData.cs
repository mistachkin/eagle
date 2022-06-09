/*
 * NamespaceData.cs --
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
    [ObjectId("e823179a-212a-4469-87c3-a72a5ed6ffa1")]
    public interface INamespaceData : IIdentifier, IHaveInterpreter
    {
        INamespace Parent { get; set; }
        IResolve Resolve { get; set; }
        ICallFrame VariableFrame { get; set; }
        string Unknown { get; set; }
    }
}
