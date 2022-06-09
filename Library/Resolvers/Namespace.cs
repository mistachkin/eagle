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
    [ObjectId("f5af315a-2fef-482f-9e47-0af558369bea")]
    public class Namespace : Core
    {
        #region Public Constructors
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
