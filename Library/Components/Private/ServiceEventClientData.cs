/*
 * ServiceEventClientData.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class encapsulates the contextual data needed to service the event
    /// queue for an interpreter, including the event selection flags, priority,
    /// target thread, processing limit, and various behavioral options.
    /// </summary>
    [ObjectId("09506e1f-cd05-4e7c-9c0c-84f75c9b378d")]
    internal sealed class ServiceEventClientData : ClientData, IGetInterpreter
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class that wraps the specified opaque
        /// client data value.
        /// </summary>
        /// <param name="data">
        /// The opaque client data value to be wrapped by this object instance.
        /// </param>
        private ServiceEventClientData(
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
        /// client data value and captures the supplied event servicing context.
        /// </summary>
        /// <param name="data">
        /// The opaque client data value to be wrapped by this object instance.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter whose event queue is to be serviced.
        /// </param>
        /// <param name="eventFlags">
        /// The flags used to select which events should be serviced.
        /// </param>
        /// <param name="priority">
        /// The minimum priority of events that should be serviced.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose events should be serviced, or
        /// null to service events regardless of their originating thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to service, or zero for no limit.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to prevent the local script cancellation flags from being
        /// reset while servicing events.
        /// </param>
        /// <param name="noGlobalCancel">
        /// Non-zero to prevent the global script cancellation flags from being
        /// reset while servicing events.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop servicing events as soon as one of them fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to treat an empty event queue as an error condition.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the events being serviced are associated with a user
        /// interface.
        /// </param>
        public ServiceEventClientData(
            object data,
            Interpreter interpreter,
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            bool noCancel,
            bool noGlobalCancel,
            bool stopOnError,
            bool errorOnEmpty,
            bool userInterface
            )
            : this(data)
        {
            this.interpreter = interpreter;
            this.eventFlags = eventFlags;
            this.priority = priority;
            this.threadId = threadId;
            this.limit = limit;
            this.noCancel = noCancel;
            this.noGlobalCancel = noGlobalCancel;
            this.stopOnError = stopOnError;
            this.errorOnEmpty = errorOnEmpty;
            this.userInterface = userInterface;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// The interpreter whose event queue is to be serviced.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// Gets the interpreter whose event queue is to be serviced.
        /// </summary>
        public Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The flags used to select which events should be serviced.
        /// </summary>
        private EventFlags eventFlags;

        /// <summary>
        /// Gets or sets the flags used to select which events should be
        /// serviced.
        /// </summary>
        public EventFlags EventFlags
        {
            get { return eventFlags; }
            set { eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The identifier of the thread whose events should be serviced, or
        /// null to service events regardless of their originating thread.
        /// </summary>
        private long? threadId;

        /// <summary>
        /// Gets or sets the identifier of the thread whose events should be
        /// serviced, or null to service events regardless of their originating
        /// thread.
        /// </summary>
        public long? ThreadId
        {
            get { return threadId; }
            set { threadId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The timeout interval, in milliseconds, to wait for events, or null
        /// for no timeout.
        /// </summary>
        private int? timeout;

        /// <summary>
        /// Gets or sets the timeout interval, in milliseconds, to wait for
        /// events, or null for no timeout.
        /// </summary>
        public int? Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The minimum priority of events that should be serviced.
        /// </summary>
        private EventPriority priority;

        /// <summary>
        /// Gets or sets the minimum priority of events that should be serviced.
        /// </summary>
        public EventPriority Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The maximum number of events to service, or zero for no limit.
        /// </summary>
        private int limit;

        /// <summary>
        /// Gets or sets the maximum number of events to service, or zero for no
        /// limit.
        /// </summary>
        public int Limit
        {
            get { return limit; }
            set { limit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero to prevent the local script cancellation flags from being
        /// reset while servicing events.
        /// </summary>
        private bool noCancel;

        /// <summary>
        /// Gets or sets a value indicating whether the local script
        /// cancellation flags should be left unchanged while servicing events.
        /// </summary>
        public bool NoCancel
        {
            get { return noCancel; }
            set { noCancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero to prevent the global script cancellation flags from being
        /// reset while servicing events.
        /// </summary>
        private bool noGlobalCancel;

        /// <summary>
        /// Gets or sets a value indicating whether the global script
        /// cancellation flags should be left unchanged while servicing events.
        /// </summary>
        public bool NoGlobalCancel
        {
            get { return noGlobalCancel; }
            set { noGlobalCancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero to stop servicing events as soon as one of them fails.
        /// </summary>
        private bool stopOnError;

        /// <summary>
        /// Gets or sets a value indicating whether event servicing should stop
        /// as soon as one of the events fails.
        /// </summary>
        public bool StopOnError
        {
            get { return stopOnError; }
            set { stopOnError = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero to treat an empty event queue as an error condition.
        /// </summary>
        private bool errorOnEmpty;

        /// <summary>
        /// Gets or sets a value indicating whether an empty event queue should
        /// be treated as an error condition.
        /// </summary>
        public bool ErrorOnEmpty
        {
            get { return errorOnEmpty; }
            set { errorOnEmpty = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the events being serviced are associated with a user
        /// interface.
        /// </summary>
        private bool userInterface;

        /// <summary>
        /// Gets or sets a value indicating whether the events being serviced
        /// are associated with a user interface.
        /// </summary>
        public bool UserInterface
        {
            get { return userInterface; }
            set { userInterface = value; }
        }
        #endregion
    }
}
