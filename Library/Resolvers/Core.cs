/*
 * Core.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Resolvers
{
    /// <summary>
    /// This class implements the default name resolver used by the core Eagle
    /// library to resolve variables, executable entities, namespaces, and call
    /// frames.  It derives from <see cref="Default" /> and forwards each
    /// resolution request to the corresponding protected helper method on the
    /// base class.  This resolver does not support namespaces; for
    /// namespace-aware resolution, see the <see cref="Namespace" /> derived
    /// class.
    /// </summary>
    [ObjectId("2465f7d5-091b-4466-aebb-61caa1fe00da")]
    public class Core : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the core name resolver using the
        /// specified resolver metadata.
        /// </summary>
        /// <param name="resolveData">
        /// The data used to create and identify this resolver, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Core(
            IResolveData resolveData
            )
            : base(resolveData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IResolve Members
        /// <summary>
        /// Locates the call frame that should be used to access the specified
        /// variable name, taking the supplied variable flags into account.
        /// </summary>
        /// <param name="frame">
        /// Upon input, the starting call frame; upon output, the call frame to
        /// use for the variable.  This parameter may be modified by this
        /// method.
        /// </param>
        /// <param name="varName">
        /// Upon input, the variable name to resolve; upon output, the possibly
        /// adjusted variable name.  This parameter may be modified by this
        /// method.
        /// </param>
        /// <param name="flags">
        /// Upon input, the variable flags that control resolution; upon
        /// output, the possibly adjusted variable flags.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode GetVariableFrame(
            ref ICallFrame frame,
            ref string varName,
            ref VariableFlags flags,
            ref Result error
            )
        {
            return base.GetVariableFrame2(
                ref frame, ref varName, ref flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the namespace that is currently active for the specified call
        /// frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame whose current namespace is being queried.  This
        /// parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// Upon success, this will contain the current namespace for the call
        /// frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode GetCurrentNamespace(
            ICallFrame frame,
            ref INamespace @namespace,
            ref Result error
            )
        {
            return base.GetCurrentNamespace2(
                frame, ref @namespace, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the executable entity (e.g. a command, procedure, or
        /// alias) with the specified name, for use within the specified call
        /// frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the context for the lookup.  This
        /// parameter may be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that influence how the lookup is performed (e.g.
        /// whether exact matching is required).
        /// </param>
        /// <param name="name">
        /// The name of the executable entity to resolve.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments associated with this invocation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the lookup is performed.
        /// </param>
        /// <param name="ambiguous">
        /// Upon failure due to an ambiguous (non-unique) name match, this will
        /// be set to true.
        /// </param>
        /// <param name="token">
        /// Upon success, this will contain the token identifying the resolved
        /// entity.
        /// </param>
        /// <param name="execute">
        /// Upon success, this will contain the resolved executable entity.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode GetIExecute(
            ICallFrame frame,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref IExecute execute,
            ref Result error
            )
        {
            return base.GetIExecute2(
                frame, engineFlags, name, arguments, lookupFlags,
                ref ambiguous, ref token, ref execute, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the variable with the specified name (and optional array
        /// element index) for use within the specified call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the context for the lookup.  This
        /// parameter may be null.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to resolve.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="varIndex">
        /// The array element index within the variable, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// Upon input, the variable flags that control resolution; upon
        /// output, the possibly adjusted variable flags.
        /// </param>
        /// <param name="variable">
        /// Upon success, this will contain the resolved variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode GetVariable(
            ICallFrame frame,
            string varName,
            string varIndex,
            ref VariableFlags flags,
            ref IVariable variable,
            ref Result error
            )
        {
            //
            // NOTE: Forward the call into the underlying protected method
            //       verbatim.  There are no traces specified here as they
            //       are not required under normal operation.  Furthermore,
            //       this resolver does not support namespaces.  For that,
            //       see the "Eagle._Resolvers.Namespace" derived class.
            //
            return base.GetVariable2(
                frame, varName, varIndex, null, ref flags, ref variable,
                ref error);
        }
        #endregion
    }
}
