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
    [ObjectId("cdf50325-fa00-455b-a24b-8c954a7b37d3")]
    public sealed class AsynchronousContext :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IAsynchronousContext
    {
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
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IAsynchronousContext Members
        private long threadId;
        public long ThreadId
        {
            get { return threadId; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private EngineMode engineMode;
        public EngineMode EngineMode
        {
            get { return engineMode; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            get { return text; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private EngineFlags engineFlags;
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private SubstitutionFlags substitutionFlags;
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private EventFlags eventFlags;
        public EventFlags EventFlags 
        {
            get { return eventFlags; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ExpressionFlags expressionFlags;
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private AsynchronousCallback callback;
        public AsynchronousCallback Callback
        {
            get { return callback; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode returnCode;
        public ReturnCode ReturnCode
        {
            get { return returnCode; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Result result;
        public Result Result
        {
            get { return result; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int errorLine;
        public int ErrorLine
        {
            get { return errorLine; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
