/*
 * ScriptTimeoutClientData.cs --
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

#if THREADING
using Eagle._Interfaces.Private;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("225e0b36-594a-4181-9289-768fb936b471")]
    internal sealed class ScriptTimeoutClientData : ClientData, IGetInterpreter
    {
        #region Private Constructors
        private ScriptTimeoutClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ScriptTimeoutClientData(
            object data,
            Interpreter interpreter,
#if THREADING
            IEngineContext engineContext,
#endif
            TimeoutFlags timeoutFlags,
            CancelFlags? cancelFlags,
            int timeout
            )
            : this(data)
        {
            this.interpreter = interpreter;
#if THREADING
            this.engineContext = engineContext;
#endif
            this.timeoutFlags = timeoutFlags;
            this.cancelFlags = cancelFlags;
            this.timeout = timeout;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
#if THREADING
        private IEngineContext engineContext;
        public IEngineContext EngineContext
        {
            get { return engineContext; }
            set { engineContext = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private TimeoutFlags timeoutFlags;
        public TimeoutFlags TimeoutFlags
        {
            get { return timeoutFlags; }
            set { timeoutFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private CancelFlags? cancelFlags;
        public CancelFlags? CancelFlags
        {
            get { return cancelFlags; }
            set { cancelFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int timeout;
        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }
        #endregion
    }
}
