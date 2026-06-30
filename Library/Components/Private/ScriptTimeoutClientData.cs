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
    /// <summary>
    /// This class encapsulates the contextual data needed to manage a script
    /// timeout, including the associated interpreter, engine context, timeout
    /// and cancellation flags, and the timeout interval itself.
    /// </summary>
    [ObjectId("225e0b36-594a-4181-9289-768fb936b471")]
    internal sealed class ScriptTimeoutClientData : ClientData, IGetInterpreter
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class that wraps the specified opaque
        /// client data value.
        /// </summary>
        /// <param name="data">
        /// The opaque client data value to be wrapped by this object instance.
        /// </param>
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
        /// <summary>
        /// Constructs an instance of this class that wraps the specified opaque
        /// client data value and captures the supplied script timeout context.
        /// </summary>
        /// <param name="data">
        /// The opaque client data value to be wrapped by this object instance.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the script timeout.
        /// </param>
        /// <param name="engineContext">
        /// The engine context associated with the script timeout.
        /// </param>
        /// <param name="timeoutFlags">
        /// The flags that control how the script timeout is handled.
        /// </param>
        /// <param name="cancelFlags">
        /// The flags used when canceling script evaluation due to the timeout,
        /// or null to use the default cancellation behavior.
        /// </param>
        /// <param name="timeout">
        /// The timeout interval, in milliseconds.
        /// </param>
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
        /// <summary>
        /// The interpreter associated with the script timeout.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// Gets the interpreter associated with the script timeout.
        /// </summary>
        public Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
#if THREADING
        /// <summary>
        /// The engine context associated with the script timeout.
        /// </summary>
        private IEngineContext engineContext;

        /// <summary>
        /// Gets or sets the engine context associated with the script timeout.
        /// </summary>
        public IEngineContext EngineContext
        {
            get { return engineContext; }
            set { engineContext = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that control how the script timeout is handled.
        /// </summary>
        private TimeoutFlags timeoutFlags;

        /// <summary>
        /// Gets or sets the flags that control how the script timeout is
        /// handled.
        /// </summary>
        public TimeoutFlags TimeoutFlags
        {
            get { return timeoutFlags; }
            set { timeoutFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags used when canceling script evaluation due to the timeout,
        /// or null to use the default cancellation behavior.
        /// </summary>
        private CancelFlags? cancelFlags;

        /// <summary>
        /// Gets or sets the flags used when canceling script evaluation due to
        /// the timeout, or null to use the default cancellation behavior.
        /// </summary>
        public CancelFlags? CancelFlags
        {
            get { return cancelFlags; }
            set { cancelFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The timeout interval, in milliseconds.
        /// </summary>
        private int timeout;

        /// <summary>
        /// Gets or sets the timeout interval, in milliseconds.
        /// </summary>
        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }
        #endregion
    }
}
