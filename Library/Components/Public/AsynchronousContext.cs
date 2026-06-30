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
using Eagle._Components.Public.Delegates;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class captures the state associated with a single asynchronous
    /// script evaluation request, including its originating thread, the
    /// interpreter and script text involved, the engine and substitution
    /// flags in effect, the completion callback, and the eventual result.  It
    /// implements <see cref="IAsynchronousContext" /> and is passed to the
    /// callback when the asynchronous operation completes.
    /// </summary>
    [ObjectId("cdf50325-fa00-455b-a24b-8c954a7b37d3")]
    public sealed class AsynchronousContext :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IAsynchronousContext
    {
        /// <summary>
        /// Constructs an asynchronous context capturing the full set of state
        /// describing an asynchronous script evaluation request.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread that originated the asynchronous
        /// request.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode in effect for the asynchronous operation.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the asynchronous operation.
        /// </param>
        /// <param name="text">
        /// The script text to be evaluated.  This parameter may be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags in effect for the asynchronous operation.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags in effect for the asynchronous operation.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags in effect for the asynchronous operation.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags in effect for the asynchronous operation.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke when the asynchronous operation completes.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the asynchronous operation.  This
        /// parameter may be null.
        /// </param>
        internal AsynchronousContext(
            long threadId,
            EngineMode engineMode,
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            AsynchronousCallback callback,
            IClientData clientData
            )
        {
            this.threadId = threadId;

            this.engineMode = engineMode;
            this.interpreter = interpreter;
            this.text = text;
            this.engineFlags = engineFlags;
            this.substitutionFlags = substitutionFlags;
            this.eventFlags = eventFlags;
            this.expressionFlags = expressionFlags;
            this.callback = callback;
            this.clientData = clientData;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// The interpreter associated with the asynchronous operation.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter associated with the asynchronous operation.
        /// </summary>
        public Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetClientData Members
        /// <summary>
        /// The client data associated with the asynchronous operation.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets the client data associated with the asynchronous operation.
        /// </summary>
        public IClientData ClientData
        {
            get { return clientData; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IAsynchronousContext Members
        /// <summary>
        /// The identifier of the thread that originated the asynchronous
        /// request.
        /// </summary>
        private long threadId;
        /// <summary>
        /// Gets the identifier of the thread that originated the asynchronous
        /// request.
        /// </summary>
        public long ThreadId
        {
            get { return threadId; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The engine mode in effect for the asynchronous operation.
        /// </summary>
        private EngineMode engineMode;
        /// <summary>
        /// Gets the engine mode in effect for the asynchronous operation.
        /// </summary>
        public EngineMode EngineMode
        {
            get { return engineMode; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script text to be evaluated.
        /// </summary>
        private string text;
        /// <summary>
        /// Gets the script text to be evaluated.
        /// </summary>
        public string Text
        {
            get { return text; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The engine flags in effect for the asynchronous operation.
        /// </summary>
        private EngineFlags engineFlags;
        /// <summary>
        /// Gets the engine flags in effect for the asynchronous operation.
        /// </summary>
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The substitution flags in effect for the asynchronous operation.
        /// </summary>
        private SubstitutionFlags substitutionFlags;
        /// <summary>
        /// Gets the substitution flags in effect for the asynchronous
        /// operation.
        /// </summary>
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The event flags in effect for the asynchronous operation.
        /// </summary>
        private EventFlags eventFlags;
        /// <summary>
        /// Gets the event flags in effect for the asynchronous operation.
        /// </summary>
        public EventFlags EventFlags 
        {
            get { return eventFlags; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The expression flags in effect for the asynchronous operation.
        /// </summary>
        private ExpressionFlags expressionFlags;
        /// <summary>
        /// Gets the expression flags in effect for the asynchronous operation.
        /// </summary>
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The callback to invoke when the asynchronous operation completes.
        /// </summary>
        private AsynchronousCallback callback;
        /// <summary>
        /// Gets the callback to invoke when the asynchronous operation
        /// completes.
        /// </summary>
        public AsynchronousCallback Callback
        {
            get { return callback; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The return code produced by the asynchronous operation.
        /// </summary>
        private ReturnCode returnCode;
        /// <summary>
        /// Gets the return code produced by the asynchronous operation.
        /// </summary>
        public ReturnCode ReturnCode
        {
            get { return returnCode; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The result produced by the asynchronous operation.
        /// </summary>
        private Result result;
        /// <summary>
        /// Gets the result produced by the asynchronous operation.
        /// </summary>
        public Result Result
        {
            get { return result; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The line number on which an error occurred during the asynchronous
        /// operation, or zero if none.
        /// </summary>
        private int errorLine;
        /// <summary>
        /// Gets the line number on which an error occurred during the
        /// asynchronous operation, or zero if none.
        /// </summary>
        public int ErrorLine
        {
            get { return errorLine; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the outcome of the asynchronous operation,
        /// storing its return code, result, and error line for later
        /// retrieval.
        /// </summary>
        /// <param name="returnCode">
        /// The return code produced by the asynchronous operation.
        /// </param>
        /// <param name="result">
        /// The result produced by the asynchronous operation.  This parameter
        /// may be null.
        /// </param>
        /// <param name="errorLine">
        /// The line number on which an error occurred, or zero if none.
        /// </param>
        public void SetResult(
            ReturnCode returnCode,
            Result result,
            int errorLine
            )
        {
            this.returnCode = returnCode;
            this.result = result;
            this.errorLine = errorLine;
        }
        #endregion
    }
}
