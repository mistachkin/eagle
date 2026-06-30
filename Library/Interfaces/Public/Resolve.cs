/*
 * Resolve.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by custom resolvers, which participate in
    /// resolving the call frame, namespace, command, and variable referenced by
    /// a name during script evaluation.  It composes the resolver identity and
    /// metadata (<see cref="IResolveData" />).
    /// </summary>
    [ObjectId("e77c6722-983d-4277-bada-d1850a9ccb1a")]
    public interface IResolve : IResolveData
    {
        /// <summary>
        /// Resolves the call frame that should be used when looking up the
        /// specified variable name.
        /// </summary>
        /// <param name="frame">
        /// On input, the starting call frame; on output, the resolved call
        /// frame to use for the variable.
        /// </param>
        /// <param name="varName">
        /// On input, the variable name to resolve; on output, the resolved
        /// variable name.
        /// </param>
        /// <param name="flags">
        /// On input, the flags that control how the variable is resolved; on
        /// output, the resulting flags.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode GetVariableFrame(
            ref ICallFrame frame,    /* in, out */
            ref string varName,      /* in, out */
            ref VariableFlags flags, /* in, out */
            ref Result error         /* out */
            );

        /// <summary>
        /// Resolves the current namespace associated with the specified call
        /// frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame for which to resolve the current namespace.
        /// </param>
        /// <param name="namespace">
        /// Upon success, receives the resolved current namespace.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode GetCurrentNamespace(
            ICallFrame frame,          /* in */
            ref INamespace @namespace, /* out */
            ref Result error           /* out */
            );

        /// <summary>
        /// Resolves the executable entity associated with the specified command
        /// name.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the starting context for the lookup.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// The command name to resolve.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments associated with the invocation, if any.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the lookup is performed.
        /// </param>
        /// <param name="ambiguous">
        /// Upon return, receives a value indicating whether the name matched
        /// more than one executable entity.
        /// </param>
        /// <param name="token">
        /// Upon success, receives the token identifying the resolved executable
        /// entity.
        /// </param>
        /// <param name="execute">
        /// Upon success, receives the resolved executable entity.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode GetIExecute(
            ICallFrame frame,        /* in */
            EngineFlags engineFlags, /* in */
            string name,             /* in */
            ArgumentList arguments,  /* in */
            LookupFlags lookupFlags, /* in */
            ref bool ambiguous,      /* out */
            ref long token,          /* out */
            ref IExecute execute,    /* out */
            ref Result error         /* out */
            );

        /// <summary>
        /// Resolves the variable associated with the specified variable name
        /// and optional index.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the starting context for the lookup.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to resolve.
        /// </param>
        /// <param name="varIndex">
        /// The array element index of the variable to resolve, if any.
        /// </param>
        /// <param name="flags">
        /// On input, the flags that control how the variable is resolved; on
        /// output, the resulting flags.
        /// </param>
        /// <param name="variable">
        /// Upon success, receives the resolved variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode GetVariable(
            ICallFrame frame,        /* in */
            string varName,          /* in */
            string varIndex,         /* in */
            ref VariableFlags flags, /* out */
            ref IVariable variable,  /* out */
            ref Result error         /* out */
            );
    }
}
