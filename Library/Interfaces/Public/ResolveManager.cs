/*
 * ResolveManager.cs --
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
    [ObjectId("751ae2fb-3b13-4d4a-adc1-05b02bb01f8b")]
    public interface IResolveManager
    {
        ///////////////////////////////////////////////////////////////////////
        // VARIABLE FRAME RESOLUTION (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetVariableFrameViaResolvers(
            LookupFlags lookupFlags,
            ref ICallFrame frame,
            ref string varName,
            ref VariableFlags variableFlags,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // NAMESPACE MANAGEMENT (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetGlobalNamespace(
            LookupFlags lookupFlags,
            ref INamespace @namespace,
            ref Result result
            );

        ReturnCode GetCurrentNamespaceViaResolvers(
            ICallFrame frame,
            LookupFlags lookupFlags,
            ref INamespace @namespace,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // CALL FRAME MANAGEMENT (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        bool? HasCallFrameFlags(
            ICallFrame frame,
            CallFrameFlags hasFlags,
            bool all
            );

        ReturnCode GetCallFrame(
            bool absolute,
            int level,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref ICallFrame frame,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // COMMAND & VARIABLE RESOLUTION (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        EngineFlags GetResolverEngineFlags(bool exact);

        ReturnCode GetIExecuteViaResolvers(
            ICallFrame frame,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref IExecute execute,
            ref Result error
            );

        ReturnCode GetIExecuteViaResolvers(
            ICallFrame frame,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref IExecute execute,
            ref Result error
            );

        ReturnCode GetVariableViaResolvers(
            ICallFrame frame,
            string varName,
            string varIndex,
            ref VariableFlags flags,
            ref IVariable variable,
            ref Result error
            );

        ReturnCode GetVariableViaResolversWithSplit(
            ICallFrame frame,
            string name,
            string index,
            ref string varName,
            ref string varIndex,
            ref VariableFlags variableFlags,
            ref IVariable variable,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // RESOLVER MANAGEMENT (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        bool HasResolvers(ref Result error);

        ReturnCode ResetResolvers(ref Result error);

        ReturnCode GetResolver(
            Priority priority,
            ref IResolve resolve,
            ref Result error
            );

        ReturnCode AddResolver(
            IResolve resolve,
            IClientData clientData,
            Priority priority,
            ref Result result
            );
    }
}
