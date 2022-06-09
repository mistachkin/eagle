/*
 * EventQueue.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

using EventQueueKey = Eagle._Interfaces.Public.IAnyTriplet<
    Eagle._Components.Public.EventPriority, System.DateTime, long>;

namespace Eagle._Containers.Private
{
    [ObjectId("8f97e602-afbe-42e1-946d-549421326de0")]
    internal sealed class EventQueue :
        QueueList<EventQueueKey, IEvent>, IDisposable
    {
        #region Public Constructors
        public EventQueue()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void Clear(
            bool dispose,
            bool force
            )
        {
            CheckDisposed();

            if (dispose)
            {
                foreach (KeyValuePair<EventQueueKey, IEvent> pair in this)
                {
                    IEvent @event = pair.Value;

                    if (@event == null)
                        continue;

                    if (force)
                        Event.Dispose(@event);
                    else
                        Event.MaybeDispose(@event);

                    @event = null;
                }
            }

            Clear();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(EventQueue).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            ) /* throw */
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    Clear(disposing, true);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~EventQueue()
        {
            Dispose(false);
        }
        #endregion
    }
}
