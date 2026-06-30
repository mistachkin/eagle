/*
 * Namespace.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Resolvers
{
    /// <summary>
    /// This class implements a namespace-aware name resolver, extending
    /// <see cref="Core" /> to resolve variables and executable entities
    /// relative to the namespace that is current for a given call frame.  It
    /// honors any per-namespace custom resolver and falls back to the base
    /// (global) resolution behavior when a name does not belong to a non-global
    /// namespace.
    /// </summary>
    [ObjectId("f5af315a-2fef-482f-9e47-0af558369bea")]
    public class Namespace : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the namespace-aware resolver and makes the
        /// specified namespace the current one for the given call frame.
        /// </summary>
        /// <param name="resolveData">
        /// The data used to create and identify this resolver, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        /// <param name="frame">
        /// The call frame for which the specified namespace should be made
        /// current.  This parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The namespace to make current for the specified call frame.  This
        /// parameter may be null.
        /// </param>
        public Namespace(
            IResolveData resolveData,
            ICallFrame frame,
            INamespace @namespace
            )
            : base(resolveData)
        {
            NamespaceOps.SetCurrent(base.Interpreter, frame, @namespace);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// Determines the namespace, custom resolver, and trailing (tail) name
        /// that should be used when resolving an executable entity with the
        /// specified name, for use within the specified call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the context for the lookup.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The (possibly namespace-qualified) name of the executable entity to
        /// resolve.  This parameter should not be null.
        /// </param>
        /// <param name="resolve">
        /// Upon return, this will contain the custom resolver associated with
        /// the resolved namespace, if any.
        /// </param>
        /// <param name="tail">
        /// Upon return, this will contain the trailing (unqualified) portion
        /// of the name, relative to the resolved namespace.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The namespace associated with the specified name, or null if it
        /// could not be determined.
        /// </returns>
        protected virtual INamespace GetNamespaceForIExecute(
            ICallFrame frame,     /* in */
            string name,          /* in */
            ref IResolve resolve, /* out */
            ref string tail,      /* out */
            ref Result error      /* out */
            )
        {
            return NamespaceOps.GetForIExecute(
                base.Interpreter, frame, name, ref resolve, ref tail,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines the namespace, custom resolver, and trailing (tail) name
        /// that should be used when resolving a variable with the specified
        /// name, for use within the specified call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the context for the lookup.  This
        /// parameter may be null.
        /// </param>
        /// <param name="varName">
        /// The (possibly namespace-qualified) name of the variable to resolve.
        /// This parameter should not be null.
        /// </param>
        /// <param name="resolve">
        /// Upon return, this will contain the custom resolver associated with
        /// the resolved namespace, if any.
        /// </param>
        /// <param name="tail">
        /// Upon return, this will contain the trailing (unqualified) portion
        /// of the name, relative to the resolved namespace.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The namespace associated with the specified name, or null if it
        /// could not be determined.
        /// </returns>
        protected virtual INamespace GetNamespaceForVariable(
            ICallFrame frame,     /* in */
            string varName,       /* in */
            ref IResolve resolve, /* out */
            ref string tail,      /* out */
            ref Result error      /* out */
            )
        {
            return NamespaceOps.GetForVariable(
                base.Interpreter, frame, varName, ref resolve, ref tail,
                ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IResolve Members
        /// <summary>
        /// Locates the call frame that should be used to access the specified
        /// variable name, resolving relative to the namespace current for the
        /// given frame and honoring any per-namespace custom resolver.
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
            INamespace @namespace = NamespaceOps.GetCurrent(
                base.Interpreter, frame);

            if (@namespace != null)
            {
                IResolve resolve = @namespace.Resolve;

                if (resolve != null)
                {
                    return resolve.GetVariableFrame(
                        ref frame, ref varName, ref flags, ref error);
                }
            }

            return NamespaceOps.GetVariableFrame(
                base.Interpreter, ref frame, ref varName, ref flags,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the namespace that is currently active for the specified call
        /// frame, honoring any per-namespace custom resolver.
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
            INamespace localNamespace = NamespaceOps.GetCurrent(
                base.Interpreter, frame);

            if (localNamespace != null)
            {
                IResolve resolve = localNamespace.Resolve;

                if (resolve != null)
                {
                    return resolve.GetCurrentNamespace(
                        frame, ref @namespace, ref error);
                }
                else
                {
                    @namespace = localNamespace;
                    return ReturnCode.Ok;
                }
            }

            error = "no current namespace for call frame";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the executable entity (e.g. a command, procedure, or
        /// alias) with the specified name relative to the namespace current
        /// for the given call frame, falling back to base resolution when the
        /// name does not belong to a non-global namespace.
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
            if (!EngineFlagOps.HasGlobalOnly(engineFlags))
            {
                IResolve resolve = null;
                string tail = null;

                INamespace @namespace = GetNamespaceForIExecute(
                    frame, name, ref resolve, ref tail, ref error);

                if (@namespace != null)
                {
                    if (resolve != null)
                    {
                        return resolve.GetIExecute(
                            frame, engineFlags, tail, arguments, lookupFlags,
                            ref ambiguous, ref token, ref execute, ref error);
                    }
                    else
                    {
                        Interpreter interpreter = base.Interpreter;

                        if (!NamespaceOps.IsGlobal(interpreter, @namespace))
                        {
                            string qualifiedName =
                                NamespaceOps.MakeQualifiedName(
                                    interpreter, @namespace, name);

                            if (base.GetIExecute(frame, 
                                    engineFlags | EngineFlags.ExactMatch,
                                    qualifiedName, arguments, lookupFlags,
                                    ref ambiguous, ref token, ref execute,
                                    ref error) == ReturnCode.Ok)
                            {
                                return ReturnCode.Ok;
                            }
                        }
                    }
                }
            }

            return base.GetIExecute(
                frame, engineFlags, name, arguments, lookupFlags,
                ref ambiguous, ref token, ref execute, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the variable with the specified name (and optional array
        /// element index) relative to the namespace current for the given call
        /// frame, falling back to base resolution when the name does not belong
        /// to a non-global namespace.
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
            IResolve resolve = null;
            string tail = null;

            INamespace @namespace = GetNamespaceForVariable(
                frame, varName, ref resolve, ref tail, ref error);

            if (@namespace != null)
            {
                if (resolve != null)
                {
                    return resolve.GetVariable(
                        frame, tail, varIndex, ref flags, ref variable,
                        ref error);
                }
                else
                {
                    Interpreter interpreter = base.Interpreter;

                    if (!NamespaceOps.IsGlobal(interpreter, @namespace))
                    {
                        frame = @namespace.VariableFrame;

                        return base.GetVariable(
                            frame, tail, varIndex, ref flags, ref variable,
                            ref error);
                    }
                }
            }

            return base.GetVariable(
                frame, varName, varIndex, ref flags, ref variable,
                ref error);
        }
        #endregion
    }
}
