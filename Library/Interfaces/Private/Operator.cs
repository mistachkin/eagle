/*
 * Operator.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by every expression operator that can be
    /// added to and dispatched by an Eagle interpreter.  It is an aggregate
    /// that composes the operator identity and metadata
    /// (<see cref="IOperatorData" />), mutable per-operator state
    /// (<see cref="IState" />), the argument-based execution entry point
    /// (<see cref="IExecuteArgument" />), and usage tracking
    /// (<see cref="IUsageData" />).  It adds no members of its own.
    /// </summary>
    [ObjectId("3c6f33aa-2613-442d-96a3-62f874d26bd3")]
    internal interface IOperator : IOperatorData, IState, IExecuteArgument, IUsageData
    {
        // nothing.
    }
}
