/*
 * UnknownCallback.cs --
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
    [ObjectId("43b49d67-f5bf-4d22-b564-6651105bd67f")]
    public interface IUnknownCallback
    {
        ReturnCode Unknown(
            Interpreter interpreter, // TODO: Change to use the IInterpreter type.
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref IExecute execute,
            ref Result error
        );
    }
}
