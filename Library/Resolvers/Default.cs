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
    [ObjectId("fd02ce56-fef3-4932-9d1e-22e6115a362e")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IResolve
    {
        #region Public Constructors
        public Default(
            IResolveData resolveData
            )
        {
            kind = IdentifierKind.Resolve;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroups(this);

            if (resolveData != null)
            {
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
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter;
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IResolve Members
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
