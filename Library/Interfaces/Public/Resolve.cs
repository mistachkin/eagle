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
    [ObjectId("e77c6722-983d-4277-bada-d1850a9ccb1a")]
    public interface IResolve : IResolveData
    {
        [Throw(true)]
        ReturnCode GetVariableFrame(
            ref ICallFrame frame,    /* in, out */
            ref string varName,      /* in, out */
            ref VariableFlags flags, /* in, out */
            ref Result error         /* out */
            );

        [Throw(true)]
        ReturnCode GetCurrentNamespace(
            ICallFrame frame,          /* in */
            ref INamespace @namespace, /* out */
            ref Result error           /* out */
            );

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
