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
    /// <summary>
    /// This class represents a last-in, first-out (LIFO) stack built on top of
    /// the generic <see cref="List{T}" />.  The end of the list is treated as
    /// the top of the stack, allowing items to be pushed, peeked, and popped
    /// while still exposing all of the underlying list functionality.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the elements stored on the stack.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("30b96fc2-aabf-467a-b377-a17e364705f5")]
    public class StackList<T> : List<T>
    {
        /// <summary>
        /// Constructs a new instance of this class that is empty and has the
        /// default initial capacity.
        /// </summary>
        public StackList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class that is empty and has the
        /// specified initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements that the new stack can initially store
        /// before resizing is required.
        /// </param>
        public StackList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class that contains the elements
        /// copied from the specified collection, pushed in enumeration order.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are copied onto the new stack.
        /// </param>
        public StackList(
            IEnumerable<T> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the stack contains no elements.
        /// True if the stack is empty; otherwise, false.
        /// </summary>
        public virtual bool IsEmpty
        {
            get { return (this.Count == 0); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the zero-based index of the element at the top of the stack,
        /// which is the last element of the underlying list.
        /// </summary>
        public virtual int Top
        {
            get { return (this.Count - 1); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the element at the top of the stack without
        /// removing it.
        /// </summary>
        /// <returns>
        /// The element currently at the top of the stack.
        /// </returns>
        public virtual T Peek() /* throw */
        {
            return this[this.Top];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the element at the specified depth below the top
        /// of the stack without removing it.
        /// </summary>
        /// <param name="index">
        /// The number of elements below the top of the stack at which to peek;
        /// zero refers to the element at the top of the stack.
        /// </param>
        /// <returns>
        /// The element located at the specified depth below the top of the
        /// stack.
        /// </returns>
        public virtual T Peek(int index) /* throw */
        {
            return this[this.Top - index];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the element at the top of the stack.
        /// </summary>
        /// <returns>
        /// The element that was removed from the top of the stack.
        /// </returns>
        public virtual T Pop()
        {
            T item = Peek();
            this.RemoveAt(this.Top);
            return item;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method inserts an element onto the top of the stack.
        /// </summary>
        /// <param name="item">
        /// The element to push onto the top of the stack.
        /// </param>
        public virtual void Push(T item)
        {
            this.Add(item);
        }
    }
}
