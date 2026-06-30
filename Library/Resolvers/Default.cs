/*
 * Default.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Resolvers
{
    /// <summary>
    /// This class provides the default implementation of the
    /// <see cref="IResolve" /> interface and serves as the base class for the
    /// Eagle name resolvers.  It stores the common identification and
    /// configuration data for a resolver and supplies protected helper methods
    /// that perform the actual resolution of variables, executable entities,
    /// namespaces, and call frames.  The public <see cref="IResolve" /> methods
    /// are not functional here; derived classes (such as <see cref="Core" />)
    /// override them to forward to the protected helpers.
    /// </summary>
    [ObjectId("fd02ce56-fef3-4932-9d1e-22e6115a362e")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IResolve
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default name resolver, optionally
        /// initializing it from the specified resolver metadata.
        /// </summary>
        /// <param name="resolveData">
        /// The data used to create and identify this resolver, such as its
        /// name, group, and flags.  This parameter may be null.
        /// </param>
        public Default(
            IResolveData resolveData
            )
        {
            kind = IdentifierKind.Resolve;

            if ((resolveData == null) ||
                !FlagOps.HasFlags(resolveData.Flags,
                    ResolveFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (resolveData != null)
            {
                id = resolveData.Id;

                EntityOps.MaybeSetupId(this);

                EntityOps.MaybeSetGroup(
                    this, resolveData.Group);

                name = resolveData.Name;
                description = resolveData.Description;
                clientData = resolveData.ClientData;
                interpreter = resolveData.Interpreter;
                token = resolveData.Token;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// Provides the default implementation used to locate the call frame
        /// that should be used to access the specified variable name, taking
        /// the supplied variable flags into account.  This method exists for
        /// legacy compatibility with the Eagle beta releases.
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
        protected virtual ReturnCode GetVariableFrame2(
            ref ICallFrame frame,
            ref string varName,
            ref VariableFlags flags,
            ref Result error
            )
        {
            Interpreter localInterpreter = interpreter;

            if (localInterpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: This is used for legacy compatibility with the Eagle
            //       beta releases.
            //
            frame = localInterpreter.GetVariableFrame(
                frame, ref varName, ref flags); /* EXEMPT */

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Provides the default implementation used to obtain the namespace
        /// that is currently active for the specified call frame.  By default
        /// this always returns the global namespace of the interpreter.
        /// </summary>
        /// <param name="frame">
        /// The call frame whose current namespace is being queried.  This
        /// parameter is not used by this method and may be null.
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
        protected virtual ReturnCode GetCurrentNamespace2(
            ICallFrame frame, /* NOT USED */
            ref INamespace @namespace,
            ref Result error
            )
        {
            Interpreter localInterpreter = interpreter;

            if (localInterpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            @namespace = localInterpreter.GlobalNamespace;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Provides the default implementation used to resolve the executable
        /// entity (e.g. a command, procedure, or alias) with the specified
        /// name, for use within the specified call frame.  Inexact (unique
        /// prefix) matching is used unless exact matching is required by the
        /// engine flags.
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
        /// parameter is not used by this method and may be null.
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
        protected virtual ReturnCode GetIExecute2(
            ICallFrame frame,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments, /* NOT USED */
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref IExecute execute,
            ref Result error
            )
        {
            Interpreter localInterpreter = interpreter;

            if (localInterpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: Lookup the command or procedure to execute.  We
            //       use inexact (unique prefix) matching here unless
            //       we are forbidden from doing so; in that case, we
            //       use exact matching.
            //
            if (EngineFlagOps.HasExactMatch(engineFlags))
            {
                return localInterpreter.GetAnyIExecute(
                    frame, engineFlags | EngineFlags.GetHidden,
                    name, lookupFlags, ref token, ref execute,
                    ref error);
            }
            else
            {
                //
                // NOTE: Include hidden commands in the resolution
                //       phase here because the policy decisions about
                //       whether or not to execute them are not made
                //       here.
                //
                return localInterpreter.MatchAnyIExecute(
                    frame, engineFlags | EngineFlags.MatchHidden,
                    name, lookupFlags, ref ambiguous, ref token,
                    ref execute, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Provides the default implementation used to resolve the variable
        /// with the specified name (and optional array element index) for use
        /// within the specified call frame.
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
        /// <param name="traces">
        /// The list of variable traces to apply during resolution, if any.
        /// This parameter may be null.
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
        protected virtual ReturnCode GetVariable2(
            ICallFrame frame,
            string varName,
            string varIndex,
            TraceList traces,
            ref VariableFlags flags,
            ref IVariable variable,
            ref Result error
            )
        {
            Interpreter localInterpreter = interpreter;

            if (localInterpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            return localInterpreter.GetVariable(
                frame, varName, varIndex, traces, ref flags, ref variable,
                ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this resolver.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this resolver.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The kind of identifier represented by this resolver.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this resolver.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier (GUID) of this resolver.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier (GUID) of this resolver.
        /// </summary>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The extra data associated with this resolver, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra data associated with this resolver, if any.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The object group that this resolver belongs to, if any.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the object group that this resolver belongs to, if any.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this resolver, if any.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this resolver, if
        /// any.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        /// <summary>
        /// The interpreter that this resolver is associated with.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets or sets the interpreter that this resolver is associated with.
        /// </summary>
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The token that identifies this resolver within the interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token that identifies this resolver within the
        /// interpreter.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IResolveData Members
        /// <summary>
        /// The flags that control the behavior of this resolver.
        /// </summary>
        private ResolveFlags flags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this resolver.
        /// </summary>
        public virtual ResolveFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IResolve Members
        /// <summary>
        /// Locates the call frame that should be used to access the specified
        /// variable name.  This default implementation always reports a
        /// not-implemented error; derived classes are expected to override it.
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
        public virtual ReturnCode GetVariableFrame(
            ref ICallFrame frame,
            ref string varName,
            ref VariableFlags flags,
            ref Result error
            )
        {
            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the namespace that is currently active for the specified call
        /// frame.  This default implementation always reports a not-implemented
        /// error; derived classes are expected to override it.
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
        public virtual ReturnCode GetCurrentNamespace(
            ICallFrame frame,
            ref INamespace @namespace,
            ref Result error
            )
        {
            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the executable entity (e.g. a command, procedure, or
        /// alias) with the specified name.  This default implementation always
        /// reports a not-implemented error; derived classes are expected to
        /// override it.
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
        public virtual ReturnCode GetIExecute(
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
            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the variable with the specified name (and optional array
        /// element index).  This default implementation always reports a
        /// not-implemented error; derived classes are expected to override it.
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
        public virtual ReturnCode GetVariable(
            ICallFrame frame,
            string varName,
            string varIndex,
            ref VariableFlags flags,
            ref IVariable variable,
            ref Result error
            )
        {
            error = "not implemented";
            return ReturnCode.Error;
        }
        #endregion
    }
}
