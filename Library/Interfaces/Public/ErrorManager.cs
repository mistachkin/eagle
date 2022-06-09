/*
 * ErrorManager.cs --
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
    [ObjectId("169da22e-94a6-4252-a7c1-92db09365dd0")]
    public interface IErrorManager
    {
        ///////////////////////////////////////////////////////////////////////
        // SCRIPT ERROR HANDLING
        ///////////////////////////////////////////////////////////////////////

        int ErrorLine { get; set; }

        // string ErrorCode { get; set; } // TODO: Maybe?
        // string ErrorInfo { get; set; } // TODO: Maybe?

        ReturnCode ResetErrorInformation(VariableFlags flags, bool all,
            bool strict, ref ResultList errors);

        ReturnCode CopyErrorInformation(VariableFlags flags,
            ref Result result);

        ReturnCode CopyErrorInformation(VariableFlags flags, bool strict,
            ref Result result);

        ReturnCode CopyErrorInformation(VariableFlags flags, bool strict,
            ref Result errorCode, ref Result errorInfo);

        ReturnCode CopyErrorInformation(VariableFlags flags, bool strict,
            ref Result errorCode, ref Result errorInfo, ref ResultList errors);
    }
}
