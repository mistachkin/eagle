/*
 * StackList.cs --
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

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("30b96fc2-aabf-467a-b377-a17e364705f5")]
    public class StackList<T> : List<T>
    {
        public StackList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StackList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StackList(
            IEnumerable<T> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsEmpty
        {
            get { return (this.Count == 0); }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int Top
        {
            get { return (this.Count - 1); }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual T Peek() /* throw */
        {
            return this[this.Top];
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual T Peek(int index) /* throw */
        {
            return this[this.Top - index];
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual T Pop()
        {
            T item = Peek();
            this.RemoveAt(this.Top);
            return item;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void Push(T item)
        {
            this.Add(item);
        }
    }
}
