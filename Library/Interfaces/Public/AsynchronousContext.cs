/*
 * AsynchronousContext.cs --
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
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("03ffae1f-84af-48d9-ab9c-8945a1918bb7")]
    public interface IAsynchronousContext : IGetInterpreter, IGetClientData
    {
        long ThreadId { get; }                       /* in */

        EngineMode EngineMode { get; }               /* in */
        string Text { get; }                         /* in */
        EngineFlags EngineFlags { get; }             /* in */
        SubstitutionFlags SubstitutionFlags { get; } /* in */
        EventFlags EventFlags { get; }               /* in */
        ExpressionFlags ExpressionFlags { get; }     /* in */
        AsynchronousCallback Callback { get; }       /* in */

        ReturnCode ReturnCode { get; }               /* out */
        Result Result { get; }                       /* out */
        int ErrorLine { get; }                       /* out */

        //
        // NOTE: This method is used to set the result.
        //
        void SetResult(ReturnCode returnCode, Result result, int errorLine);
    }
}
