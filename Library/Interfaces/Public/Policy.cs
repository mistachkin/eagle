/*
 * Policy.cs --
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
    /// This interface is implemented by every policy that can be added to
    /// and evaluated by an Eagle interpreter.  It is an aggregate that
    /// composes the policy identity and metadata
    /// (<see cref="IPolicyData" />), dynamic-execute callback support
    /// (<see cref="IDynamicExecuteCallback" />), the execution entry point
    /// used to evaluate the policy (<see cref="IExecute" />), and
    /// setup/teardown support (<see cref="ISetup" />).  It declares no
    /// members of its own.
    /// </summary>
    [ObjectId("c94c6eed-6666-4e8e-91e3-d154dbc5b738")]
    public interface IPolicy : IPolicyData, IDynamicExecuteCallback, IExecute, ISetup
    {
        // nothing.
    }
}
