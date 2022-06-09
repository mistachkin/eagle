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
    [ObjectId("09506e1f-cd05-4e7c-9c0c-84f75c9b378d")]
    internal sealed class ServiceEventClientData : ClientData, IGetInterpreter
    {
        #region Private Constructors
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
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private EventFlags eventFlags;
        public EventFlags EventFlags
        {
            get { return eventFlags; }
            set { eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long? threadId;
        public long? ThreadId
        {
            get { return threadId; }
            set { threadId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventPriority priority;
        public EventPriority Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int limit;
        public int Limit
        {
            get { return limit; }
            set { limit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noCancel;
        public bool NoCancel
        {
            get { return noCancel; }
            set { noCancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noGlobalCancel;
        public bool NoGlobalCancel
        {
            get { return noGlobalCancel; }
            set { noGlobalCancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool stopOnError;
        public bool StopOnError
        {
            get { return stopOnError; }
            set { stopOnError = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool errorOnEmpty;
        public bool ErrorOnEmpty
        {
            get { return errorOnEmpty; }
            set { errorOnEmpty = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool userInterface;
        public bool UserInterface
        {
            get { return userInterface; }
            set { userInterface = value; }
        }
        #endregion
    }
}
