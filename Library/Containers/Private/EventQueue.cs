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
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

using EventQueueKey = Eagle._Interfaces.Public.IAnyTriplet<
    Eagle._Components.Public.EventPriority, System.DateTime, long>;

using EventPair = System.Collections.Generic.KeyValuePair<
    Eagle._Interfaces.Public.IAnyTriplet<Eagle._Components.Public.EventPriority,
    System.DateTime, long>, Eagle._Interfaces.Public.IEvent>;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a priority-ordered queue of events awaiting
    /// processing by the interpreter.  Each event is keyed by a triplet of its
    /// priority, its scheduled date and time, and a monotonically increasing
    /// sequence number.  It extends the underlying ordered queue list and is
    /// disposable; disposing the queue optionally disposes the events it still
    /// holds.
    /// </summary>
    [ObjectId("8f97e602-afbe-42e1-946d-549421326de0")]
    internal sealed class EventQueue :
        QueueList<EventQueueKey, IEvent>, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty event queue.
        /// </summary>
        public EventQueue()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method removes all events from the queue, optionally disposing
        /// the events that it contains before they are removed.
        /// </summary>
        /// <param name="dispose">
        /// Non-zero if the events contained in the queue should be disposed
        /// prior to being removed.
        /// </param>
        /// <param name="force">
        /// Non-zero if each event should be disposed unconditionally; zero if
        /// each event should be disposed only when it is eligible for disposal.
        /// This parameter has no effect unless <paramref name="dispose" /> is
        /// non-zero.
        /// </param>
        public void Clear(
            bool dispose,
            bool force
            )
        {
            CheckDisposed();

            if (dispose)
            {
                foreach (EventPair pair in this)
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
        /// <summary>
        /// Stores a value indicating whether this event queue has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this event queue has already been
        /// disposed.  It is called at the start of most members to guard against
        /// use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this event queue has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(EventQueue).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this event queue.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
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
        /// <summary>
        /// This method releases all resources held by this event queue and
        /// suppresses finalization.
        /// </summary>
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this event queue, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~EventQueue()
        {
            Dispose(false);
        }
        #endregion
    }
}
