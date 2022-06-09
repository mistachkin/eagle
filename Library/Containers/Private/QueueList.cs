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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("734830a0-5d89-48cf-876f-8815e788f5cc")]
    internal class QueueList<TKey, TValue> : SortedList<TKey, TValue>
    {
        #region Private Helper Methods
        private TKey GetKey(
            int index
            )
        {
            return this.Keys[index]; /* throw */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        public virtual bool IsEmpty
        {
            get { return (this.Count == 0); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Indexers
        public virtual TValue this[int index] /* throw */
        {
            get { return this[GetKey(index)]; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public virtual TValue Peek() /* throw */
        {
            return this[0];
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual TValue Dequeue()
        {
            TValue value = Peek();
            this.RemoveAt(0);
            return value;
        }

        ///////////////////////////////////////////////////////////////////////

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
