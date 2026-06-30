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
    /// <summary>
    /// This interface defines the contract for the context associated with an
    /// asynchronous script evaluation.  It carries the input parameters for
    /// the operation (e.g. the script text and the various engine flags) as
    /// well as the output values produced once the operation completes (e.g.
    /// the return code, result, and error line).
    /// </summary>
    [ObjectId("03ffae1f-84af-48d9-ab9c-8945a1918bb7")]
    public interface IAsynchronousContext : IGetInterpreter, IGetClientData
    {
        /// <summary>
        /// Gets the identifier of the thread that initiated the asynchronous
        /// operation.
        /// </summary>
        long ThreadId { get; }                       /* in */

        /// <summary>
        /// Gets the engine mode in effect for the asynchronous operation.
        /// </summary>
        EngineMode EngineMode { get; }               /* in */
        /// <summary>
        /// Gets the script text to be evaluated by the asynchronous operation.
        /// </summary>
        string Text { get; }                         /* in */
        /// <summary>
        /// Gets the engine flags in effect for the asynchronous operation.
        /// </summary>
        EngineFlags EngineFlags { get; }             /* in */
        /// <summary>
        /// Gets the substitution flags in effect for the asynchronous
        /// operation.
        /// </summary>
        SubstitutionFlags SubstitutionFlags { get; } /* in */
        /// <summary>
        /// Gets the event flags in effect for the asynchronous operation.
        /// </summary>
        EventFlags EventFlags { get; }               /* in */
        /// <summary>
        /// Gets the expression flags in effect for the asynchronous operation.
        /// </summary>
        ExpressionFlags ExpressionFlags { get; }     /* in */
        /// <summary>
        /// Gets the callback to be invoked when the asynchronous operation
        /// completes.
        /// </summary>
        AsynchronousCallback Callback { get; }       /* in */

        /// <summary>
        /// Gets the return code produced by the completed asynchronous
        /// operation.
        /// </summary>
        ReturnCode ReturnCode { get; }               /* out */
        /// <summary>
        /// Gets the result produced by the completed asynchronous operation.
        /// </summary>
        Result Result { get; }                       /* out */
        /// <summary>
        /// Gets the one-based line number at which an error occurred during the
        /// completed asynchronous operation, if any.
        /// </summary>
        int ErrorLine { get; }                       /* out */

        //
        // NOTE: This method is used to set the result.
        //
        /// <summary>
        /// Sets the result of the asynchronous operation.
        /// </summary>
        /// <param name="returnCode">
        /// The return code produced by the operation.
        /// </param>
        /// <param name="result">
        /// The result produced by the operation.
        /// </param>
        /// <param name="errorLine">
        /// The one-based line number at which an error occurred, or zero if no
        /// error occurred.
        /// </param>
        void SetResult(ReturnCode returnCode, Result result, int errorLine);
    }
}
