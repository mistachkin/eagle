/*
 * Function.cs --
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
    /// This interface is implemented by every mathematical expression
    /// function that can be added to and evaluated by an Eagle interpreter
    /// (the functions usable inside <c>expr</c>).  It is an aggregate that
    /// composes the function identity (<see cref="IFunctionData" />), mutable
    /// per-function state (<see cref="IState" />), the argument-based
    /// execution entry point (<see cref="IExecuteArgument" />), and usage
    /// tracking (<see cref="IUsageData" />).  Most plugin authors derive from
    /// the default function base class rather than implementing this
    /// interface directly.  See <c>core_language.md</c> for expression
    /// semantics.
    /// </summary>
    [ObjectId("93812cb4-afcf-4600-bb7d-7de9adeb6a4f")]
    public interface IFunction : IFunctionData, IState, IExecuteArgument, IUsageData
    {
        // nothing.
    }
}
