/*
 * DisplayHost.cs --
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
    /// <summary>
    /// This interface is implemented by hosts that support display-related
    /// operations.  It is an aggregate that composes box drawing
    /// (<see cref="IBoxHost" />), color support (<see cref="IColorHost" />),
    /// cursor positioning (<see cref="IPositionHost" />), size queries
    /// (<see cref="ISizeHost" />), and text output
    /// (<see cref="IWriteHost" />).
    /// </summary>
    [ObjectId("63e4c2f5-f4f9-4eed-b17e-dae050d790e5")]
    public interface IDisplayHost :
            IBoxHost, IColorHost, IPositionHost, ISizeHost, IWriteHost
    {
        // nothing.
    }
}
