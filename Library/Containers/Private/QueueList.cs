/*
 * QueueList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Collections.Generic;
using Eagle._Attributes;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a queue of values that is ordered by their
    /// associated keys.  It extends the standard generic sorted list with
    /// queue-style operations that act on the entry with the smallest key.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the keys used to order the entries in the queue.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the values stored in the queue.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("734830a0-5d89-48cf-876f-8815e788f5cc")]
    internal class QueueList<TKey, TValue> : SortedList<TKey, TValue>
    {
        #region Private Helper Methods
        /// <summary>
        /// This method retrieves the key located at the specified position in
        /// the sorted order of the keys.
        /// </summary>
        /// <param name="index">
        /// The zero-based position, in the sorted order, of the key to return.
        /// </param>
        /// <returns>
        /// The key located at the specified position.
        /// </returns>
        private TKey GetKey(
            int index
            )
        {
            return this.Keys[index]; /* throw */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether the queue is empty.
        /// </summary>
        public virtual bool IsEmpty
        {
            get { return (this.Count == 0); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Indexers
        /// <summary>
        /// Gets the value whose key is located at the specified position in the
        /// sorted order of the keys.
        /// </summary>
        /// <param name="index">
        /// The zero-based position, in the sorted order, of the value to return.
        /// </param>
        /// <returns>
        /// The value whose key is located at the specified position.
        /// </returns>
        public virtual TValue this[int index] /* throw */
        {
            get { return this[GetKey(index)]; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method returns the value at the front of the queue (i.e. the
        /// value with the smallest key) without removing it.
        /// </summary>
        /// <returns>
        /// The value at the front of the queue.
        /// </returns>
        public virtual TValue Peek() /* throw */
        {
            return this[0];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the value at the front of the queue
        /// (i.e. the value with the smallest key).
        /// </summary>
        /// <returns>
        /// The value that was removed from the front of the queue.
        /// </returns>
        public virtual TValue Dequeue()
        {
            TValue value = Peek();
            this.RemoveAt(0);
            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds a value to the queue, using the specified key to
        /// determine its position in the sorted order.
        /// </summary>
        /// <param name="key">
        /// The key that determines the position of the value within the queue.
        /// </param>
        /// <param name="value">
        /// The value to add to the queue.
        /// </param>
        public virtual void Enqueue(
            TKey key,
            TValue value
            )
        {
            this.Add(key, value);
        }
        #endregion
    }
}
