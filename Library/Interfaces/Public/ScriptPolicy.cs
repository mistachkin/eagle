/*
 * ScriptPolicy.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by a script-based policy, which decides
    /// whether a particular command, script, file, or stream should be
    /// allowed to execute.  It extends <see cref="IExecute" />, providing
    /// the entry point that performs the policy decision, and exposes
    /// metadata about the command the policy applies to and the interpreter
    /// that evaluates it.
    /// </summary>
    [ObjectId("d3146201-50a4-4671-b34b-f2cacb7a06ef")]
    public interface IScriptPolicy : IExecute
    {
        /// <summary>
        /// Gets the flags that describe the behavior and applicability of this
        /// policy.
        /// </summary>
        PolicyFlags Flags { get; }
        /// <summary>
        /// Gets the type of the command this policy applies to, if any.
        /// </summary>
        Type CommandType { get; }
        /// <summary>
        /// Gets the token identifying the command this policy applies to, if
        /// any.
        /// </summary>
        long CommandToken { get; }
        /// <summary>
        /// Gets the interpreter used to evaluate this policy, if any.
        /// </summary>
        Interpreter PolicyInterpreter { get; }
        /// <summary>
        /// Gets the script text that implements this policy, if any.
        /// </summary>
        string Text { get; }
    }
}
