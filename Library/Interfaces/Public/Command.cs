/*
 * Command.cs --
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
    /// This interface is implemented by every command that can be added to
    /// and dispatched by an Eagle interpreter.  It is an aggregate that
    /// composes the command identity (<see cref="ICommandData" />), mutable
    /// per-command state (<see cref="IState" />), the execution entry point
    /// (<see cref="IExecute" />), dynamic-execute callback support
    /// (<see cref="IDynamicExecuteCallback" />), sub-command ensemble
    /// dispatch (<see cref="IEnsemble" />), per-ensemble policy hooks
    /// (<see cref="IPolicyEnsemble" />), syntax metadata
    /// (<see cref="ISyntax" />), and usage tracking
    /// (<see cref="IUsageData" />).  Most plugin authors derive from the
    /// default command base class rather than implementing this interface
    /// directly.  See <c>core_language.md</c> for command semantics.
    /// </summary>
    [ObjectId("c187713a-3b67-4b38-88ec-d7c37a8fb901")]
    public interface ICommand : ICommandData, IState, IDynamicExecuteCallback, IExecute, IEnsemble, IPolicyEnsemble, ISyntax, IUsageData
    {
        // nothing.
    }
}
