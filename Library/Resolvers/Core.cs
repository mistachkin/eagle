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
    [ObjectId("2465f7d5-091b-4466-aebb-61caa1fe00da")]
    public class Core : Default
    {
        #region Public Constructors
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
