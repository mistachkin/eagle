/*
 * VariableContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    [ObjectId("d37ec74b-cad9-4cd4-9b5d-c07006f45122")]
    internal interface IVariableContext : IThreadContext
    {
        CallStack CallStack { get; set; }

        ICallFrame GlobalFrame { get; set; }
        ICallFrame GlobalScopeFrame { get; set; }
        ICallFrame CurrentFrame { get; set; }
        ICallFrame ProcedureFrame { get; set; }
        ICallFrame UplevelFrame { get; set; }

        ICallFrame CurrentGlobalFrame { get; }

        ITraceInfo TraceInfo { get; set; }

        void Free(bool global);
    }
}
