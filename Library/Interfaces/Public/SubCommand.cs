/*
 * SubCommand.cs --
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
    /// This interface is implemented by every sub-command that can be added
    /// to and dispatched by a command ensemble within an Eagle interpreter.
    /// It is an aggregate that composes the sub-command metadata
    /// (<see cref="ISubCommandData" />), dynamic-execute callback support
    /// (<see cref="IDynamicExecuteCallback" />), delegate metadata
    /// (<see cref="IDelegateData" />), the execution entry point
    /// (<see cref="IExecute" />), sub-command ensemble dispatch
    /// (<see cref="IEnsemble" />), per-ensemble policy hooks
    /// (<see cref="IPolicyEnsemble" />), syntax metadata
    /// (<see cref="ISyntax" />), and usage tracking
    /// (<see cref="IUsageData" />).
    /// </summary>
    [ObjectId("c0757ae1-4732-44db-8a1f-ed7e925834dc")]
    public interface ISubCommand : ISubCommandData, IDynamicExecuteCallback, IDelegateData, IExecute, IEnsemble, IPolicyEnsemble, ISyntax, IUsageData
    {
        // nothing.
    }
}
